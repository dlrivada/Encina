```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error      | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------:|-----------:|----------:|------:|-------:|----------:|------------:|
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     |   889.53 ns |   3.824 ns |  3.577 ns |  1.08 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |    14.79 ns |   0.351 ns |  0.329 ns |  0.02 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     |   815.20 ns |   1.959 ns |  1.736 ns |  0.99 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 1,520.49 ns |   8.098 ns |  7.179 ns |  1.84 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 1,506.65 ns |   5.622 ns |  4.984 ns |  1.83 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     |   824.19 ns |   2.671 ns |  2.368 ns |  1.00 | 0.0296 |     496 B |        1.00 |
|                              |            |                |             |             |             |            |           |       |        |           |             |
| ToCreatedResponse_Success    | ShortRun   | 3              | 1           | 3           |   923.62 ns | 190.063 ns | 10.418 ns |  1.13 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | ShortRun   | 3              | 1           | 3           |    15.14 ns |   3.851 ns |  0.211 ns |  0.02 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | ShortRun   | 3              | 1           | 3           |   812.63 ns |  41.498 ns |  2.275 ns |  1.00 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | ShortRun   | 3              | 1           | 3           | 1,524.50 ns |  73.068 ns |  4.005 ns |  1.87 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | ShortRun   | 3              | 1           | 3           | 1,547.80 ns |  34.371 ns |  1.884 ns |  1.90 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | ShortRun   | 3              | 1           | 3           |   815.82 ns |  19.092 ns |  1.046 ns |  1.00 | 0.0296 |     496 B |        1.00 |
