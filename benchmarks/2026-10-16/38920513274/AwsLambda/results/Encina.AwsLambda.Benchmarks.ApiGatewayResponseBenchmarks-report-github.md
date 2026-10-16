```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     |   801.59 ns |  10.220 ns |  9.560 ns |  1.43 |    0.02 | 0.0057 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |    11.75 ns |   0.064 ns |  0.057 ns |  0.02 |    0.00 | 0.0006 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     |   567.36 ns |   3.402 ns |  3.183 ns |  1.01 |    0.01 | 0.0057 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 1,151.86 ns |   3.960 ns |  3.704 ns |  2.05 |    0.01 | 0.0134 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 1,155.99 ns |   6.108 ns |  5.713 ns |  2.06 |    0.01 | 0.0134 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     |   562.52 ns |   3.656 ns |  3.053 ns |  1.00 |    0.01 | 0.0057 |     496 B |        1.00 |
|                              |            |                |             |             |             |            |           |       |         |        |           |             |
| ToCreatedResponse_Success    | ShortRun   | 3              | 1           | 3           |   792.11 ns | 285.172 ns | 15.631 ns |  1.39 |    0.03 | 0.0057 |     480 B |        0.97 |
| ToNoContentResponse_Success  | ShortRun   | 3              | 1           | 3           |    12.68 ns |   6.805 ns |  0.373 ns |  0.02 |    0.00 | 0.0006 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | ShortRun   | 3              | 1           | 3           |   563.53 ns |  48.037 ns |  2.633 ns |  0.99 |    0.01 | 0.0057 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | ShortRun   | 3              | 1           | 3           | 1,130.42 ns | 135.286 ns |  7.415 ns |  1.98 |    0.02 | 0.0134 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | ShortRun   | 3              | 1           | 3           | 1,136.13 ns | 125.273 ns |  6.867 ns |  1.99 |    0.02 | 0.0134 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | ShortRun   | 3              | 1           | 3           |   570.37 ns |  78.916 ns |  4.326 ns |  1.00 |    0.01 | 0.0057 |     496 B |        1.00 |
