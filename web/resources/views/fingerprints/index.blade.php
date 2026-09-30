@extends('layouts.app')

@section('title', 'Daftar Terdaftar')

@section('content')
  <div class="card">
    <h2>Daftar Sidik Jari Terdaftar</h2>
    <table>
      <thead>
        <tr><th>Nama</th><th>Terdaftar pada</th></tr>
      </thead>
      <tbody>
        @forelse ($fingerprints as $fingerprint)
          <tr>
            <td>{{ $fingerprint->nama }}</td>
            <td>{{ $fingerprint->created_at->format('d M Y H:i') }}</td>
          </tr>
        @empty
          <tr class="empty-row"><td colspan="2">Belum ada yang terdaftar.</td></tr>
        @endforelse
      </tbody>
    </table>
  </div>
@endsection
