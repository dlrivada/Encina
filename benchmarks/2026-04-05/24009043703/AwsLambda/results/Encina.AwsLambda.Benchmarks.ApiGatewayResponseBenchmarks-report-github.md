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
| ToApiGatewayResponse_Success | DefaultJob | Default        | Default     | Default     | 16           | Default     |        783.22 ns | 2.343 ns | 2.192 ns |  1.00 | 0.0296 |     496 B |        1.00 |
| ToApiGatewayResponse_Error   | DefaultJob | Default        | Default     | Default     | 16           | Default     |      1,416.87 ns | 6.194 ns | 5.491 ns |  1.81 | 0.0725 |    1232 B |        2.48 |
| ToCreatedResponse_Success    | DefaultJob | Default        | Default     | Default     | 16           | Default     |        879.68 ns | 3.142 ns | 2.939 ns |  1.12 | 0.0286 |     480 B |        0.97 |
| ToNoContentResponse_Success  | DefaultJob | Default        | Default     | Default     | 16           | Default     |         13.59 ns | 0.034 ns | 0.028 ns |  0.02 | 0.0029 |      48 B |        0.10 |
| ToHttpApiResponse_Success    | DefaultJob | Default        | Default     | Default     | 16           | Default     |        778.45 ns | 2.853 ns | 2.382 ns |  0.99 | 0.0296 |     496 B |        1.00 |
| ToHttpApiResponse_Error      | DefaultJob | Default        | Default     | Default     | 16           | Default     |      1,445.43 ns | 2.715 ns | 2.539 ns |  1.85 | 0.0725 |    1232 B |        2.48 |
|                              |            |                |             |             |              |             |                  |          |          |       |        |           |             |
| ToApiGatewayResponse_Success | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 27,765,660.00 ns |       NA | 0.000 ns |  1.00 |      - |     496 B |        1.00 |
| ToApiGatewayResponse_Error   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 41,269,303.00 ns |       NA | 0.000 ns |  1.49 |      - |    1232 B |        2.48 |
| ToCreatedResponse_Success    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 28,867,505.00 ns |       NA | 0.000 ns |  1.04 |      - |     480 B |        0.97 |
| ToNoContentResponse_Success  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  1,928,575.00 ns |       NA | 0.000 ns |  0.07 |      - |      48 B |        0.10 |
| ToHttpApiResponse_Success    | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 28,273,176.00 ns |       NA | 0.000 ns |  1.02 |      - |     496 B |        1.00 |
| ToHttpApiResponse_Error      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 40,973,582.00 ns |       NA | 0.000 ns |  1.48 |      - |    1232 B |        2.48 |
