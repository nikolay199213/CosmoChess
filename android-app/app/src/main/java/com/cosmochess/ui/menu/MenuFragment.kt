package com.cosmochess.ui.menu

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android:view.ViewGroup
import android.widget.TextView
import androidx.fragment.app.Fragment
import androidx.navigation.fragment.findNavController
import com.cosmochess.ChessApplication
import com.cosmochess.R
import com.google.android.material.button.MaterialButton

class MenuFragment : Fragment() {

    private val authRepository by lazy {
        (requireActivity().application as ChessApplication).authRepository
    }

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View? {
        return inflater.inflate(R.layout.fragment_menu, container, false)
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)

        val welcomeText = view.findViewById<TextView>(R.id.welcomeText)
        val playBotButton = view.findViewById<MaterialButton>(R.id.playBotButton)
        val playOnlineButton = view.findViewById<MaterialButton>(R.id.playOnlineButton)
        val logoutButton = view.findViewById<MaterialButton>(R.id.logoutButton)

        val user = authRepository.getCurrentUser()
        if (user != null) {
            welcomeText.text = "Welcome, ${user.username}!"
        }

        playBotButton.setOnClickListener {
            findNavController().navigate(R.id.action_menu_to_bot_setup)
        }

        playOnlineButton.setOnClickListener {
            // TODO: Implement online multiplayer game list
            android.widget.Toast.makeText(
                context,
                "Online multiplayer coming soon!",
                android.widget.Toast.LENGTH_SHORT
            ).show()
        }

        logoutButton.setOnClickListener {
            authRepository.logout()
            findNavController().navigate(R.id.action_menu_to_login)
        }
    }
}
