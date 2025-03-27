import { initializeApp } from "https://www.gstatic.com/firebasejs/9.17.2/firebase-app.js";
import { getAuth, signInWithPopup, GoogleAuthProvider } from "https://www.gstatic.com/firebasejs/9.17.2/firebase-auth.js";

// Firebase configuration
const firebaseConfig = {
    apiKey: "AIzaSyAX9k2yxOkyothtS07rYSnWfpMjO4p5nYA",
    authDomain: "jamper-finance.firebaseapp.com",
    projectId: "jamper-finance",
    storageBucket: "jamper-finance.firebasestorage.app",
    messagingSenderId: "746677604268",
    appId: "1:746677604268:web:b70182ecf0354eb3ea3409"
};

// Initialize Firebase
const app = initializeApp(firebaseConfig);
const auth = getAuth(app);

window.signInWithGoogle = async function (dotNetObjectReference) {
    const provider = new GoogleAuthProvider();

    try {
        const result = await signInWithPopup(auth, provider);
        const user = result.user;
        console.log("Google user signed in:", user);
        const userInfo = {
            displayName: user.displayName,
            email: user.email,
            photoURL: user.photoURL,
            uid: user.uid
        };

        // Pass the user information to Blazor
        try {
            await dotNetObjectReference.invokeMethodAsync('OnGoogleLoginSuccess', userInfo);
            console.log("OnGoogleLoginSuccess called successfully.");
        } catch (error) {
            console.error("Error calling OnGoogleLoginSuccess:", error);
            alert("Error sending user information to the server."); // Inform user of error.
        }

        // alert(`Welcome, ${user.displayName}`);
        // window.location.href = "/dashboard-page"; // Redirect after login
    } catch (error) {
        console.error("Google sign-in error:", error);
        alert("Google sign-in failed: " + error.message);
    }
};

window.deleteCurrentUser = async function () {
    const auth = getAuth();
    try {
        const user = auth.currentUser;
        if (user) {
            await user.delete();
            alert("Account deleted successfully!");
            window.location.href = "/"; // Redirect to home page after deletion
        } else {
            alert("No user is currently signed in.");
        }
    } catch (error) {
        console.error("Error deleting user:", error);
        alert("Failed to delete account: " + error.message);
    }
};
