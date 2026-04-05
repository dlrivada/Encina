```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                       | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean            | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |----------------:|---------:|--------:|------:|-------:|----------:|------------:|
| SimpleRoute_SingleCondition  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,995.0 ns | 13.41 ns | 8.87 ns |  1.00 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,394.0 ns | 14.60 ns | 9.66 ns |  0.70 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,390.3 ns |  6.25 ns | 4.14 ns |  0.70 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      2,086.5 ns |  9.60 ns | 6.35 ns |  1.05 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      2,091.8 ns | 10.16 ns | 6.04 ns |  1.05 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        361.3 ns |  3.10 ns | 2.05 ns |  0.18 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        637.0 ns |  5.96 ns | 3.55 ns |  0.32 | 0.0782 |    1320 B |        1.34 |
|                              |            |                |             |             |              |             |                 |          |         |       |        |           |             |
| SimpleRoute_SingleCondition  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 34,102,615.0 ns |       NA | 0.00 ns |  1.00 |      - |    1192 B |        1.00 |
| ComplexRoute_FirstMatch      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 31,565,131.0 ns |       NA | 0.00 ns |  0.93 |      - |     792 B |        0.66 |
| ComplexRoute_DefaultFallback | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 32,216,378.0 ns |       NA | 0.00 ns |  0.94 |      - |     792 B |        0.66 |
| ManyRoutes_FirstMatch        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 35,100,651.0 ns |       NA | 0.00 ns |  1.03 |      - |    1232 B |        1.03 |
| ManyRoutes_LateMatch         | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 34,556,554.0 ns |       NA | 0.00 ns |  1.01 |      - |    1232 B |        1.03 |
| BuildDefinition_Simple       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    589,903.0 ns |       NA | 0.00 ns |  0.02 |      - |     584 B |        0.49 |
| BuildDefinition_Complex      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  1,053,680.0 ns |       NA | 0.00 ns |  0.03 |      - |    1320 B |        1.11 |
