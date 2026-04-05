```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.82GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                       | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean             | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |-----------------:|---------:|---------:|------:|-------:|----------:|------------:|
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     | 16           | Default     |        819.31 ns | 4.393 ns | 3.894 ns |  1.00 | 0.0296 |     496 B |        1.00 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 16           | Default     |      1,517.05 ns | 7.877 ns | 6.983 ns |  1.85 | 0.0725 |    1232 B |        2.48 |
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     | 16           | Default     |        883.67 ns | 2.365 ns | 2.212 ns |  1.08 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     | 16           | Default     |         14.21 ns | 0.209 ns | 0.195 ns |  0.02 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     | 16           | Default     |        803.72 ns | 2.328 ns | 2.177 ns |  0.98 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 16           | Default     |      1,538.37 ns | 8.178 ns | 7.649 ns |  1.88 | 0.0725 |    1232 B |        2.48 |
|                              |            |                |             |             |              |             |                  |          |          |       |        |           |             |
| ToApiGatewayResponse_Success | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 29,133,013.00 ns |       NA | 0.000 ns |  1.00 |      - |     496 B |        1.00 |
| ToApiGatewayResponse_Error   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 41,834,427.00 ns |       NA | 0.000 ns |  1.44 |      - |    1232 B |        2.48 |
| ToCreatedResponse_Success    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 29,705,290.00 ns |       NA | 0.000 ns |  1.02 |      - |     480 B |        0.97 |
| ToNoContentResponse_Success  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,019,666.00 ns |       NA | 0.000 ns |  0.07 |      - |      48 B |        0.10 |
| ToHttpApiResponse_Success    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 28,408,041.00 ns |       NA | 0.000 ns |  0.98 |      - |     496 B |        1.00 |
| ToHttpApiResponse_Error      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 41,638,933.00 ns |       NA | 0.000 ns |  1.43 |      - |    1232 B |        2.48 |
