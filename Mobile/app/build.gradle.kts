plugins {
    alias(libs.plugins.android.application)
}

android {
    namespace = "com.example.mobile"
    compileSdk = 36 // Se seu Android Studio reclamar, pode ajustar para 34 ou a versão mais recente que você tiver instalada

    defaultConfig {
        applicationId = "com.example.mobile"
        minSdk = 24
        targetSdk = 36 // Pode ajustar para 34 se necessário
        versionCode = 1
        versionName = "1.0"

        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"
    }

    buildTypes {
        release {
            isMinifyEnabled = false
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
        }
    }
    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_11
        targetCompatibility = JavaVersion.VERSION_11
    }
}

dependencies {

    implementation(libs.appcompat)
    implementation(libs.material)
    implementation(libs.activity)
    implementation(libs.constraintlayout)

    // Dependência para fazer as chamadas de rede (HTTP)
    implementation("com.android.volley:volley:1.2.1")

    // Dependência para facilitar a conversão de JSON para objetos Java
    implementation("com.google.code.gson:gson:2.9.0")

    // Dependência para a comunicação em tempo real com SignalR
    implementation("com.microsoft.signalr:signalr:5.0.10") // Verifique se há uma versão mais recente se desejar

    implementation("io.reactivex.rxjava2:rxjava:2.2.21")

    testImplementation(libs.junit)
    androidTestImplementation(libs.ext.junit)
    androidTestImplementation(libs.espresso.core)
}