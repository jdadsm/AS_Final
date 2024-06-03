import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';

const Profile = ({setIsLoggedIn}) => {
    const [userData, setUserData] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const navigate = useNavigate();


    useEffect(() => {
        const fetchUserData = async () => {
            try {
                const response = await fetch('/getUserInfo');
                if (response.ok) {
                    const data = await response.json();
                    setUserData(data);
                } else {
                    setError('Failed to fetch user data');
                }
            } catch (error) {
                console.error('Error:', error);
                setError('Failed to fetch user data');
            } finally {
                setLoading(false);
            }
        };
        fetchUserData();
    }, []);

    const handleLogout = () => {
        setIsLoggedIn(false);
        fetch('/logout', {
            method: 'POST',
        }).then(response => {
            if (response.ok) {
                console.log('Logged out successfully');
                navigate('/');
            } else {
                console.error('Failed to logout');
            }
        }).catch(error => {
            console.error('Error:', error);
        });
    };

    if (loading) {
        return <p>Loading...</p>;
    }

    if (error) {
        return <p>Error: {error}</p>;
    }

    return (
        <div>
            <h2>User Profile</h2>
            {userData && (
                <div>
                    <p>Name: {userData.userName}</p>
                    <p>Email: {userData.email}</p>
                    <p>Date of Birth: {userData.birthDay}</p>
                    <p>Phone Number: {userData.phoneNumber}</p>

                </div>
            )}
            <button onClick={handleLogout}>Logout</button>
        </div>
    );
};

export default Profile;
