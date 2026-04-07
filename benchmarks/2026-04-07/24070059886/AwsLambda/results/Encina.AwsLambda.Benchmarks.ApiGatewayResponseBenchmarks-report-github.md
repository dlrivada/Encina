```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                       | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean             | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |-----------------:|---------:|---------:|------:|-------:|----------:|------------:|
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     | 16           | Default     |        790.76 ns | 2.375 ns | 2.221 ns |  1.00 | 0.0296 |     496 B |        1.00 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 16           | Default     |      1,486.49 ns | 6.301 ns | 5.586 ns |  1.88 | 0.0725 |    1232 B |        2.48 |
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     | 16           | Default     |        875.24 ns | 1.703 ns | 1.593 ns |  1.11 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     | 16           | Default     |         13.41 ns | 0.033 ns | 0.029 ns |  0.02 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     | 16           | Default     |        789.48 ns | 1.697 ns | 1.417 ns |  1.00 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 16           | Default     |      1,510.94 ns | 2.738 ns | 2.562 ns |  1.91 | 0.0725 |    1232 B |        2.48 |
|                              |            |                |             |             |              |             |                  |          |          |       |        |           |             |
| ToApiGatewayResponse_Success | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 27,847,146.00 ns |       NA | 0.000 ns |  1.00 |      - |     496 B |        1.00 |
| ToApiGatewayResponse_Error   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 40,707,779.00 ns |       NA | 0.000 ns |  1.46 |      - |    1232 B |        2.48 |
| ToCreatedResponse_Success    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 29,164,499.00 ns |       NA | 0.000 ns |  1.05 |      - |     480 B |        0.97 |
| ToNoContentResponse_Success  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  1,945,493.00 ns |       NA | 0.000 ns |  0.07 |      - |      48 B |        0.10 |
| ToHttpApiResponse_Success    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 27,618,369.00 ns |       NA | 0.000 ns |  0.99 |      - |     496 B |        1.00 |
| ToHttpApiResponse_Error      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 40,915,085.00 ns |       NA | 0.000 ns |  1.47 |      - |    1232 B |        2.48 |
