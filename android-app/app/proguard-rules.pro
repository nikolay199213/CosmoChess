# Add project specific ProGuard rules here.
# You can control the set of applied configuration files using the
# proguardFiles setting in build.gradle.

# Keep SignalR classes
-keep class com.microsoft.signalr.** { *; }

# Keep Retrofit classes
-keep class retrofit2.** { *; }
-keepattributes Signature
-keepattributes Exceptions

# Keep Gson classes
-keep class com.google.gson.** { *; }
-keepattributes *Annotation*
