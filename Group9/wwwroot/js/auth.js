function getAccessToken() {
    return localStorage.getItem("accessToken");
}

function hasToken() {
    const token = getAccessToken();
    return token !== null && token !== "";
}

function clearAuthStorage() {
    localStorage.removeItem("accessToken");
    localStorage.removeItem("refreshToken");
    localStorage.removeItem("user");
}

function setGuestNavbar() {
    document.querySelectorAll(".guest-link").forEach(x => x.style.display = "inline-block");
    document.querySelectorAll(".auth-link").forEach(x => x.style.display = "none");
}

function setAuthNavbar() {
    document.querySelectorAll(".guest-link").forEach(x => x.style.display = "none");
    document.querySelectorAll(".auth-link").forEach(x => x.style.display = "inline-block");
}

async function apiPost(url, body) {
    try {
        const response = await fetch(url, {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(body)
        });

        const data = await safeJson(response);

        return {
            ok: response.ok,
            status: response.status,
            data
        };
    } catch (error) {
        return {
            ok: false,
            status: 0,
            data: {
                message: "Không kết nối được server."
            }
        };
    }
}

async function apiGetAuth(url) {
    try {
        const response = await fetch(url, {
            method: "GET",
            headers: {
                "Authorization": "Bearer " + getAccessToken()
            }
        });

        const data = await safeJson(response);

        return {
            ok: response.ok,
            status: response.status,
            data
        };
    } catch (error) {
        return {
            ok: false,
            status: 0,
            data: {
                message: "Không kết nối được server."
            }
        };
    }
}

async function apiPostAuth(url, body) {
    try {
        const response = await fetch(url, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "Authorization": "Bearer " + getAccessToken()
            },
            body: JSON.stringify(body)
        });

        const data = await safeJson(response);

        return {
            ok: response.ok,
            status: response.status,
            data
        };
    } catch (error) {
        return {
            ok: false,
            status: 0,
            data: {
                message: "Không kết nối được server."
            }
        };
    }
}

async function apiPutAuth(url, body) {
    try {
        const response = await fetch(url, {
            method: "PUT",
            headers: {
                "Content-Type": "application/json",
                "Authorization": "Bearer " + getAccessToken()
            },
            body: JSON.stringify(body)
        });

        const data = await safeJson(response);

        return {
            ok: response.ok,
            status: response.status,
            data
        };
    } catch (error) {
        return {
            ok: false,
            status: 0,
            data: {
                message: "Không kết nối được server."
            }
        };
    }
}

async function safeJson(response) {
    try {
        return await response.json();
    } catch {
        return {};
    }
}

function showMessage(result) {
    const messageBox = document.getElementById("message");

    if (!messageBox) {
        return;
    }

    let message = "Có lỗi xảy ra.";

    if (result && result.data && result.data.message) {
        message = result.data.message;
    }

    if (result && result.data && result.data.error) {
        message += " " + result.data.error;
    }

    messageBox.className = "message error";
    messageBox.innerText = message;
}

function showSuccess(message) {
    const messageBox = document.getElementById("message");

    if (!messageBox) {
        return;
    }

    messageBox.className = "message success";
    messageBox.innerText = message;
}

async function checkLoginStatus() {
    if (!hasToken()) {
        clearAuthStorage();
        setGuestNavbar();
        return false;
    }

    const result = await apiGetAuth("/api/auth/me");

    if (!result.ok) {
        clearAuthStorage();
        setGuestNavbar();
        return false;
    }

    setAuthNavbar();
    return true;
}

async function requireGuestPage() {
    const isLoggedIn = await checkLoginStatus();

    if (isLoggedIn) {
        window.location.href = "/auth-ui/profile";
        return false;
    }

    return true;
}

async function requireAuthPage() {
    const isLoggedIn = await checkLoginStatus();

    if (!isLoggedIn) {
        window.location.href = "/auth-ui/login";
        return false;
    }

    return true;
}

async function logoutFromNavbar() {
    if (hasToken()) {
        await apiPostAuth("/api/auth/logout", {});
    }

    clearAuthStorage();
    setGuestNavbar();

    window.location.href = "/auth-ui/login";
}

document.addEventListener("DOMContentLoaded", async function () {
    await checkLoginStatus();
});