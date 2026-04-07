```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                       | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean             | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |-----------------:|---------:|---------:|------:|-------:|----------:|------------:|
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     | 16           | Default     |        794.50 ns | 3.071 ns | 2.873 ns |  1.00 | 0.0296 |     496 B |        1.00 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 16           | Default     |      1,471.65 ns | 5.886 ns | 5.506 ns |  1.85 | 0.0725 |    1232 B |        2.48 |
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     | 16           | Default     |        871.76 ns | 3.299 ns | 3.086 ns |  1.10 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     | 16           | Default     |         13.89 ns | 0.077 ns | 0.069 ns |  0.02 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     | 16           | Default     |        760.89 ns | 1.587 ns | 1.407 ns |  0.96 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 16           | Default     |      1,534.64 ns | 2.444 ns | 2.041 ns |  1.93 | 0.0725 |    1232 B |        2.48 |
|                              |            |                |             |             |              |             |                  |          |          |       |        |           |             |
| ToApiGatewayResponse_Success | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 28,824,875.00 ns |       NA | 0.000 ns |  1.00 |      - |     496 B |        1.00 |
| ToApiGatewayResponse_Error   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 42,512,169.00 ns |       NA | 0.000 ns |  1.47 |      - |    1232 B |        2.48 |
| ToCreatedResponse_Success    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 29,732,412.00 ns |       NA | 0.000 ns |  1.03 |      - |     480 B |        0.97 |
| ToNoContentResponse_Success  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  1,955,245.00 ns |       NA | 0.000 ns |  0.07 |      - |      48 B |        0.10 |
| ToHttpApiResponse_Success    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 28,389,031.00 ns |       NA | 0.000 ns |  0.98 |      - |     496 B |        1.00 |
| ToHttpApiResponse_Error      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 42,370,133.00 ns |       NA | 0.000 ns |  1.47 |      - |    1232 B |        2.48 |
