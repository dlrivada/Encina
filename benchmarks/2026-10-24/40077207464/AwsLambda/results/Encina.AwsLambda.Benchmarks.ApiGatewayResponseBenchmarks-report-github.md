```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     |   903.95 ns |   2.370 ns | 2.217 ns |  1.33 |    0.00 | 0.0057 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |    15.93 ns |   0.174 ns | 0.163 ns |  0.02 |    0.00 | 0.0006 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     |   678.63 ns |   4.098 ns | 3.422 ns |  1.00 |    0.01 | 0.0057 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 1,374.67 ns |   5.455 ns | 4.836 ns |  2.02 |    0.01 | 0.0134 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 1,372.92 ns |   6.838 ns | 6.062 ns |  2.02 |    0.01 | 0.0134 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     |   679.54 ns |   1.938 ns | 1.813 ns |  1.00 |    0.00 | 0.0057 |     496 B |        1.00 |
|                              |            |                |             |             |             |            |          |       |         |        |           |             |
| ToCreatedResponse_Success    | ShortRun   | 3              | 1           | 3           |   913.80 ns |  43.074 ns | 2.361 ns |  1.30 |    0.01 | 0.0057 |     480 B |        0.97 |
| ToNoContentResponse_Success  | ShortRun   | 3              | 1           | 3           |    15.61 ns |   3.478 ns | 0.191 ns |  0.02 |    0.00 | 0.0006 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | ShortRun   | 3              | 1           | 3           |   682.15 ns |  67.648 ns | 3.708 ns |  0.97 |    0.01 | 0.0057 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | ShortRun   | 3              | 1           | 3           | 1,353.97 ns |  87.150 ns | 4.777 ns |  1.92 |    0.02 | 0.0134 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | ShortRun   | 3              | 1           | 3           | 1,351.27 ns |  21.680 ns | 1.188 ns |  1.92 |    0.01 | 0.0134 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | ShortRun   | 3              | 1           | 3           |   704.64 ns | 108.606 ns | 5.953 ns |  1.00 |    0.01 | 0.0057 |     496 B |        1.00 |
