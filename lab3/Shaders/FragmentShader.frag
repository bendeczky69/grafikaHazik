#version 330 core
out vec4 FragColor;

uniform vec3 uLightColor;
uniform vec3 uLightPos;
uniform vec3 uViewPos;

uniform float uShininess;

// harom elemu vektorokkent megadott phong egyutthatok
uniform vec3 uAmbientStrength;
uniform vec3 uDiffuseStrength;
uniform vec3 uSpecularStrength;

in vec4 outCol;
in vec3 outNormal;
in vec3 outWorldPosition;

void main()
{
    // normalizalas a vilagkoordinata rendszerben
    vec3 norm = normalize(outNormal);
    vec3 lightDir = normalize(uLightPos - outWorldPosition);
    vec3 viewDir = normalize(uViewPos - outWorldPosition);
    vec3 reflectDir = reflect(-lightDir, norm);

    // kornyezeti feny szamitasa komponensenkent
    vec3 ambient = uAmbientStrength * uLightColor;

    // diffuz feny szamitasa komponensenkent
    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = diff * uDiffuseStrength * uLightColor;

    // spekularis csillogas szamitasa komponensenkent
    float specFactor = pow(max(dot(viewDir, reflectDir), 0.0), uShininess);
    vec3 specular = specFactor * uSpecularStrength * uLightColor;

    // osszesitett phong megvilagitas szorzasa az objektum csucsszinevel
    vec3 result = (ambient + diffuse + specular) * outCol.rgb;

    FragColor = vec4(result, outCol.w);
}