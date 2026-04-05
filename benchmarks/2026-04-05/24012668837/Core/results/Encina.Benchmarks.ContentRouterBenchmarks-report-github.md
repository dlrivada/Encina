```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                       | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean            | Error   | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |----------------:|--------:|--------:|------:|-------:|----------:|------------:|
| SimpleRoute_SingleCondition  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      2,081.0 ns | 4.59 ns | 3.04 ns |  1.00 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,448.6 ns | 4.89 ns | 3.24 ns |  0.70 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,416.5 ns | 7.71 ns | 5.10 ns |  0.68 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      2,131.4 ns | 7.82 ns | 5.17 ns |  1.02 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      2,152.1 ns | 7.03 ns | 4.18 ns |  1.03 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        389.7 ns | 1.64 ns | 1.09 ns |  0.19 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        666.9 ns | 4.91 ns | 3.25 ns |  0.32 | 0.0782 |    1320 B |        1.34 |
|                              |            |                |             |             |              |             |                 |         |         |       |        |           |             |
| SimpleRoute_SingleCondition  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 35,235,097.0 ns |      NA | 0.00 ns |  1.00 |      - |    1192 B |        1.00 |
| ComplexRoute_FirstMatch      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 34,716,415.0 ns |      NA | 0.00 ns |  0.99 |      - |     792 B |        0.66 |
| ComplexRoute_DefaultFallback | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 33,492,523.0 ns |      NA | 0.00 ns |  0.95 |      - |     792 B |        0.66 |
| ManyRoutes_FirstMatch        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 36,860,691.0 ns |      NA | 0.00 ns |  1.05 |      - |    1232 B |        1.03 |
| ManyRoutes_LateMatch         | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 38,544,475.0 ns |      NA | 0.00 ns |  1.09 |      - |    1232 B |        1.03 |
| BuildDefinition_Simple       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |    628,006.0 ns |      NA | 0.00 ns |  0.02 |      - |     584 B |        0.49 |
| BuildDefinition_Complex      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  1,108,226.0 ns |      NA | 0.00 ns |  0.03 |      - |    1320 B |        1.11 |
