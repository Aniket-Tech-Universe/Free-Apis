// Discord authentication utilities

export const fetchDiscordUser = async () => {
  try {
    // Check if user has Discord info in localStorage
    const storedUser = localStorage.getItem('discord_user');
    if (storedUser) {
      return JSON.parse(storedUser);
    }
    
    // TODO: In the future, you could check for a session token here
    // and fetch the user data from your API
    
    return null;
  } catch (error) {
    console.error('Error fetching Discord user:', error);
    return null;
  }
};

export const saveDiscordUser = (user: any) => {
  try {
    localStorage.setItem('discord_user', JSON.stringify(user));
  } catch (error) {
    console.error('Error saving Discord user:', error);
  }
};

export const clearDiscordUser = () => {
  try {
    localStorage.removeItem('discord_user');
  } catch (error) {
    console.error('Error clearing Discord user:', error);
  }
};
