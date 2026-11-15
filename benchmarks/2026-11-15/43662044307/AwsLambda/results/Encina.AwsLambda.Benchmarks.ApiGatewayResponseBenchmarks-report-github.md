```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     |   603.55 ns |  1.221 ns |  0.953 ns |  1.22 |    0.01 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     |    10.40 ns |  0.079 ns |  0.074 ns |  0.02 |    0.00 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     |   491.07 ns |  4.583 ns |  4.287 ns |  1.00 |    0.01 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 1,116.84 ns |  6.844 ns |  6.402 ns |  2.26 |    0.02 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 1,132.90 ns |  9.739 ns |  9.110 ns |  2.30 |    0.03 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     |   493.50 ns |  4.700 ns |  4.397 ns |  1.00 |    0.01 | 0.0296 |     496 B |        1.00 |
|                              |            |                |             |             |             |           |           |       |         |        |           |             |
| ToCreatedResponse_Success    | MediumRun  | 15             | 2           | 10          |   564.96 ns |  4.938 ns |  6.923 ns |  1.14 |    0.03 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | MediumRun  | 15             | 2           | 10          |    10.81 ns |  0.174 ns |  0.256 ns |  0.02 |    0.00 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | MediumRun  | 15             | 2           | 10          |   499.92 ns |  5.354 ns |  7.506 ns |  1.01 |    0.03 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | MediumRun  | 15             | 2           | 10          | 1,107.34 ns | 11.655 ns | 16.715 ns |  2.23 |    0.06 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Error   | MediumRun  | 15             | 2           | 10          | 1,109.07 ns |  9.504 ns | 14.226 ns |  2.23 |    0.05 | 0.0725 |    1232 B |        2.48 |
| ToApiGatewayResponse_Success | MediumRun  | 15             | 2           | 10          |   496.96 ns |  7.107 ns | 10.638 ns |  1.00 |    0.03 | 0.0296 |     496 B |        1.00 |
