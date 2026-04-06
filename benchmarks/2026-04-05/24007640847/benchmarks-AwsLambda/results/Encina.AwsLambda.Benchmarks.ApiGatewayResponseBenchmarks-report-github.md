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
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     | 16           | Default     |        771.69 ns | 1.281 ns | 1.198 ns |  1.00 | 0.0296 |     496 B |        1.00 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 16           | Default     |      1,499.98 ns | 3.928 ns | 3.482 ns |  1.94 | 0.0725 |    1232 B |        2.48 |
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     | 16           | Default     |        868.51 ns | 1.899 ns | 1.684 ns |  1.13 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     | 16           | Default     |         13.76 ns | 0.086 ns | 0.081 ns |  0.02 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     | 16           | Default     |        779.42 ns | 3.141 ns | 2.785 ns |  1.01 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 16           | Default     |      1,481.31 ns | 3.233 ns | 2.866 ns |  1.92 | 0.0725 |    1232 B |        2.48 |
|                              |            |                |             |             |              |             |                  |          |          |       |        |           |             |
| ToApiGatewayResponse_Success | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 28,338,464.00 ns |       NA | 0.000 ns |  1.00 |      - |     496 B |        1.00 |
| ToApiGatewayResponse_Error   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 41,308,671.00 ns |       NA | 0.000 ns |  1.46 |      - |    1232 B |        2.48 |
| ToCreatedResponse_Success    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 29,577,830.00 ns |       NA | 0.000 ns |  1.04 |      - |     480 B |        0.97 |
| ToNoContentResponse_Success  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  1,990,503.00 ns |       NA | 0.000 ns |  0.07 |      - |      48 B |        0.10 |
| ToHttpApiResponse_Success    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 28,207,811.00 ns |       NA | 0.000 ns |  1.00 |      - |     496 B |        1.00 |
| ToHttpApiResponse_Error      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 43,961,846.00 ns |       NA | 0.000 ns |  1.55 |      - |    1232 B |        2.48 |
