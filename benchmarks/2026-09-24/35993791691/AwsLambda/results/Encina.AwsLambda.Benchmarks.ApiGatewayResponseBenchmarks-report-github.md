```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     |   679.76 ns |   2.275 ns |  1.899 ns |  1.38 |    0.01 | 0.0057 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |    14.38 ns |   0.322 ns |  0.269 ns |  0.03 |    0.00 | 0.0006 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     |   491.66 ns |   8.934 ns |  7.920 ns |  0.99 |    0.02 | 0.0057 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 1,034.06 ns |   7.845 ns |  6.954 ns |  2.09 |    0.02 | 0.0134 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 1,027.02 ns |  19.824 ns | 28.431 ns |  2.08 |    0.06 | 0.0134 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     |   494.34 ns |   2.521 ns |  2.358 ns |  1.00 |    0.01 | 0.0057 |     496 B |        1.00 |
|                              |            |                |             |             |             |            |           |       |         |        |           |             |
| ToCreatedResponse_Success    | ShortRun   | 3              | 1           | 3           |   690.72 ns | 438.876 ns | 24.056 ns |  1.31 |    0.04 | 0.0057 |     480 B |        0.97 |
| ToNoContentResponse_Success  | ShortRun   | 3              | 1           | 3           |    15.61 ns |  18.288 ns |  1.002 ns |  0.03 |    0.00 | 0.0006 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | ShortRun   | 3              | 1           | 3           |   484.50 ns |  39.430 ns |  2.161 ns |  0.92 |    0.01 | 0.0057 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | ShortRun   | 3              | 1           | 3           | 1,017.30 ns |  61.895 ns |  3.393 ns |  1.92 |    0.02 | 0.0134 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | ShortRun   | 3              | 1           | 3           | 1,033.77 ns | 280.111 ns | 15.354 ns |  1.95 |    0.03 | 0.0134 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | ShortRun   | 3              | 1           | 3           |   529.30 ns | 136.849 ns |  7.501 ns |  1.00 |    0.02 | 0.0057 |     496 B |        1.00 |
