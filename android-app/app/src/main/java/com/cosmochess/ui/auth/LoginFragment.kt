package com.cosmochess.ui.auth

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.Toast
import androidx.fragment.app.Fragment
import androidx.lifecycle.lifecycleScope
import androidx.navigation.fragment.findNavController
import com.cosmochess.ChessApplication
import com.cosmochess.R
import com.google.android.material.button.MaterialButton
import com.google.android.material.textfield.TextInputEditText
import kotlinx.coroutines.launch

class LoginFragment : Fragment() {

    private val authRepository by lazy {
        (requireActivity().application as ChessApplication).authRepository
    }

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View? {
        return inflater.inflate(R.layout.fragment_login, container, false)
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)

        val usernameEdit = view.findViewById<TextInputEditText>(R.id.usernameEdit)
        val passwordEdit = view.findViewById<TextInputEditText>(R.id.passwordEdit)
        val loginButton = view.findViewById<MaterialButton>(R.id.loginButton)
        val registerLink = view.findViewById<View>(R.id.registerLink)
        val progressBar = view.findViewById<View>(R.id.progressBar)

        loginButton.setOnClickListener {
            val username = usernameEdit.text.toString().trim()
            val password = passwordEdit.text.toString().trim()

            if (username.isEmpty() || password.isEmpty()) {
                Toast.makeText(context, "Please fill all fields", Toast.LENGTH_SHORT).show()
                return@setOnClickListener
            }

            progressBar.visibility = View.VISIBLE
            loginButton.isEnabled = false

            lifecycleScope.launch {
                val result = authRepository.login(username, password)

                progressBar.visibility = View.GONE
                loginButton.isEnabled = true

                if (result.isSuccess) {
                    Toast.makeText(context, "Login successful!", Toast.LENGTH_SHORT).show()
                    findNavController().navigate(R.id.action_login_to_menu)
                } else {
                    Toast.makeText(
                        context,
                        result.exceptionOrNull()?.message ?: getString(R.string.error_login),
                        Toast.LENGTH_LONG
                    ).show()
                }
            }
        }

        registerLink.setOnClickListener {
            findNavController().navigate(R.id.action_login_to_register)
        }
    }
}
