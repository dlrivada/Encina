```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.22GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |-----------:|------------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     | 431.120 ns |   5.8815 ns |  5.5016 ns |  1.09 |    0.02 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |   7.846 ns |   0.1915 ns |  0.1967 ns |  0.02 |    0.00 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     | 392.186 ns |   6.0955 ns |  5.7017 ns |  0.99 |    0.02 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 842.877 ns |   8.4450 ns |  7.4863 ns |  2.14 |    0.04 | 0.0696 |    1176 B |        2.37 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 810.069 ns |  15.7261 ns | 24.9432 ns |  2.05 |    0.07 | 0.0696 |    1176 B |        2.37 |
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     | 394.467 ns |   7.7351 ns |  7.5969 ns |  1.00 |    0.03 | 0.0296 |     496 B |        1.00 |
|                              |            |                |             |             |            |             |            |       |         |        |           |             |
| ToCreatedResponse_Success    | ShortRun   | 3              | 1           | 3           | 446.696 ns | 134.3035 ns |  7.3616 ns |  1.12 |    0.04 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | ShortRun   | 3              | 1           | 3           |   7.942 ns |   8.4113 ns |  0.4611 ns |  0.02 |    0.00 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | ShortRun   | 3              | 1           | 3           | 413.136 ns |  25.2233 ns |  1.3826 ns |  1.04 |    0.03 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | ShortRun   | 3              | 1           | 3           | 890.839 ns |  96.2908 ns |  5.2780 ns |  2.24 |    0.07 | 0.0696 |    1176 B |        2.37 |
| ToApiGatewayResponse_Error   | ShortRun   | 3              | 1           | 3           | 825.133 ns |  55.8501 ns |  3.0613 ns |  2.07 |    0.06 | 0.0696 |    1176 B |        2.37 |
| ToApiGatewayResponse_Success | ShortRun   | 3              | 1           | 3           | 398.641 ns | 252.9099 ns | 13.8628 ns |  1.00 |    0.04 | 0.0296 |     496 B |        1.00 |
