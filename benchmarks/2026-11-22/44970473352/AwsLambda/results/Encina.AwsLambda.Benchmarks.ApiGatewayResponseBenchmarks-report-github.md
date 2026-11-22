```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     |   573.45 ns |  6.428 ns |  5.699 ns |  1.17 |    0.01 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |    10.59 ns |  0.011 ns |  0.009 ns |  0.02 |    0.00 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     |   492.02 ns |  3.532 ns |  3.304 ns |  1.00 |    0.01 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 1,126.09 ns | 11.426 ns | 10.688 ns |  2.29 |    0.03 | 0.0687 |    1176 B |        2.37 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 1,125.48 ns | 16.236 ns | 15.187 ns |  2.29 |    0.03 | 0.0687 |    1176 B |        2.37 |
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     |   491.87 ns |  4.097 ns |  3.832 ns |  1.00 |    0.01 | 0.0296 |     496 B |        1.00 |
|                              |            |                |             |             |             |           |           |       |         |        |           |             |
| ToCreatedResponse_Success    | MediumRun  | 15             | 2           | 10          |   569.40 ns |  3.721 ns |  5.455 ns |  1.15 |    0.02 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | MediumRun  | 15             | 2           | 10          |    10.65 ns |  0.035 ns |  0.051 ns |  0.02 |    0.00 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | MediumRun  | 15             | 2           | 10          |   493.84 ns |  0.912 ns |  1.279 ns |  1.00 |    0.02 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | MediumRun  | 15             | 2           | 10          | 1,096.99 ns | 16.081 ns | 23.062 ns |  2.22 |    0.06 | 0.0687 |    1176 B |        2.37 |
| ToApiGatewayResponse_Error   | MediumRun  | 15             | 2           | 10          | 1,096.78 ns |  4.619 ns |  6.771 ns |  2.22 |    0.04 | 0.0687 |    1176 B |        2.37 |
| ToApiGatewayResponse_Success | MediumRun  | 15             | 2           | 10          |   494.13 ns |  5.332 ns |  7.816 ns |  1.00 |    0.02 | 0.0296 |     496 B |        1.00 |
