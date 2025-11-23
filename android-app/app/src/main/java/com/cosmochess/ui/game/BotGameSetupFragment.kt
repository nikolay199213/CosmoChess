package com.cosmochess.ui.game

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.ArrayAdapter
import android.widget.Spinner
import android.widget.Toast
import androidx.core.os.bundleOf
import androidx.fragment.app.Fragment
import androidx.lifecycle.lifecycleScope
import androidx.navigation.fragment.findNavController
import com.cosmochess.ChessApplication
import com.cosmochess.R
import com.cosmochess.data.model.BotDifficulty
import com.cosmochess.data.model.BotStyle
import com.google.android.material.button.MaterialButton
import kotlinx.coroutines.launch

class BotGameSetupFragment : Fragment() {

    private val authRepository by lazy {
        (requireActivity().application as ChessApplication).authRepository
    }

    private val gameRepository by lazy {
        (requireActivity().application as ChessApplication).gameRepository
    }

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View? {
        return inflater.inflate(R.layout.fragment_bot_game_setup, container, false)
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)

        val difficultySpinner = view.findViewById<Spinner>(R.id.difficultySpinner)
        val styleSpinner = view.findViewById<Spinner>(R.id.styleSpinner)
        val startGameButton = view.findViewById<MaterialButton>(R.id.startGameButton)
        val progressBar = view.findViewById<View>(R.id.progressBar)

        // Setup difficulty spinner
        val difficulties = BotDifficulty.values()
        val difficultyAdapter = ArrayAdapter(
            requireContext(),
            android.R.layout.simple_spinner_item,
            difficulties.map { it.displayName }
        )
        difficultyAdapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item)
        difficultySpinner.adapter = difficultyAdapter
        difficultySpinner.setSelection(2) // Default to Medium

        // Setup style spinner
        val styles = BotStyle.values()
        val styleAdapter = ArrayAdapter(
            requireContext(),
            android.R.layout.simple_spinner_item,
            styles.map { it.displayName }
        )
        styleAdapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item)
        styleSpinner.adapter = styleAdapter

        startGameButton.setOnClickListener {
            val user = authRepository.getCurrentUser()
            if (user == null) {
                Toast.makeText(context, "Please login first", Toast.LENGTH_SHORT).show()
                return@setOnClickListener
            }

            val difficulty = difficulties[difficultySpinner.selectedItemPosition]
            val style = styles[styleSpinner.selectedItemPosition]

            progressBar.visibility = View.VISIBLE
            startGameButton.isEnabled = false

            lifecycleScope.launch {
                val result = gameRepository.createBotGame(
                    creatorId = user.id,
                    difficulty = difficulty.value,
                    style = style.value
                )

                progressBar.visibility = View.GONE
                startGameButton.isEnabled = true

                if (result.isSuccess) {
                    val game = result.getOrNull()!!
                    findNavController().navigate(
                        R.id.action_bot_setup_to_game,
                        bundleOf("gameId" to game.id)
                    )
                } else {
                    Toast.makeText(
                        context,
                        result.exceptionOrNull()?.message ?: getString(R.string.error_create_game),
                        Toast.LENGTH_LONG
                    ).show()
                }
            }
        }
    }
}
