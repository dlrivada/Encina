```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     |   664.05 ns |  11.397 ns |  9.517 ns |  1.32 |    0.02 | 0.0057 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |    15.07 ns |   0.346 ns |  0.993 ns |  0.03 |    0.00 | 0.0006 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     |   497.78 ns |   1.997 ns |  1.868 ns |  0.99 |    0.01 | 0.0057 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 1,038.65 ns |  16.532 ns | 25.738 ns |  2.07 |    0.06 | 0.0134 |    1176 B |        2.37 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     |   995.63 ns |   6.305 ns |  5.265 ns |  1.98 |    0.03 | 0.0134 |    1176 B |        2.37 |
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     |   502.11 ns |   5.450 ns |  6.057 ns |  1.00 |    0.02 | 0.0057 |     496 B |        1.00 |
|                              |            |                |             |             |             |            |           |       |         |        |           |             |
| ToCreatedResponse_Success    | ShortRun   | 3              | 1           | 3           |   698.57 ns | 691.074 ns | 37.880 ns |  1.39 |    0.07 | 0.0057 |     480 B |        0.97 |
| ToNoContentResponse_Success  | ShortRun   | 3              | 1           | 3           |    14.11 ns |   3.666 ns |  0.201 ns |  0.03 |    0.00 | 0.0006 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | ShortRun   | 3              | 1           | 3           |   493.39 ns |  37.819 ns |  2.073 ns |  0.98 |    0.01 | 0.0057 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | ShortRun   | 3              | 1           | 3           |   956.86 ns |  67.586 ns |  3.705 ns |  1.90 |    0.01 | 0.0134 |    1176 B |        2.37 |
| ToApiGatewayResponse_Error   | ShortRun   | 3              | 1           | 3           | 1,043.54 ns | 538.913 ns | 29.540 ns |  2.08 |    0.05 | 0.0134 |    1176 B |        2.37 |
| ToApiGatewayResponse_Success | ShortRun   | 3              | 1           | 3           |   502.56 ns |  63.704 ns |  3.492 ns |  1.00 |    0.01 | 0.0057 |     496 B |        1.00 |
