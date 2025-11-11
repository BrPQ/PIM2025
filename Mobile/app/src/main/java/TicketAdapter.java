package com.example.mobile;

import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.Button;
import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;
import java.util.ArrayList;
import java.util.List;

public class TicketAdapter extends RecyclerView.Adapter<TicketAdapter.TicketViewHolder> {

    private List<Ticket> tickets = new ArrayList<>();
    private final OnTicketClickListener listener;

    public interface OnTicketClickListener {
        void onTicketClick(Ticket ticket);
    }

    public TicketAdapter(OnTicketClickListener listener) {
        this.listener = listener;
    }

    @NonNull
    @Override
    public TicketViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        View view = LayoutInflater.from(parent.getContext()).inflate(R.layout.list_item_ticket, parent, false);
        return new TicketViewHolder(view);
    }

    @Override
    public void onBindViewHolder(@NonNull TicketViewHolder holder, int position) {
        Ticket ticket = tickets.get(position);
        holder.bind(ticket, listener);
    }

    @Override
    public int getItemCount() {
        return tickets.size();
    }

    public void setTickets(List<Ticket> tickets) {
        this.tickets = tickets;
        notifyDataSetChanged();
    }

    static class TicketViewHolder extends RecyclerView.ViewHolder {
        private final Button ticketButton;

        public TicketViewHolder(@NonNull View itemView) {
            super(itemView);
            ticketButton = itemView.findViewById(R.id.ticketButton);
        }

        public void bind(final Ticket ticket, final OnTicketClickListener listener) {
            ticketButton.setText("TICKET#" + ticket.getChamadoId());

            // --- CORREÇÃO APLICADA AQUI ---
            // Em vez de colocar o clique na view inteira (itemView),
            // colocamos diretamente no botão que o usuário vê. Isso garante
            // que a área de clique seja exatamente a área do botão vermelho.
            ticketButton.setOnClickListener(v -> listener.onTicketClick(ticket));
            // --- FIM DA CORREÇÃO ---
        }
    }
}