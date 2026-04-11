```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev   | Median     | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |-----------:|---------:|---------:|-----------:|------:|-------:|----------:|------------:|
| SimpleRoute_SingleCondition  | Job-YFEFPZ | 10             | Default     | 3           | 2,181.1 ns |  6.40 ns |  3.81 ns | 2,181.1 ns |  1.00 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | Job-YFEFPZ | 10             | Default     | 3           | 1,619.8 ns |  3.32 ns |  1.98 ns | 1,620.3 ns |  0.74 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | Job-YFEFPZ | 10             | Default     | 3           | 1,629.1 ns |  6.98 ns |  4.62 ns | 1,628.1 ns |  0.75 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | Job-YFEFPZ | 10             | Default     | 3           | 2,265.6 ns |  8.41 ns |  4.40 ns | 2,267.1 ns |  1.04 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | Job-YFEFPZ | 10             | Default     | 3           | 2,263.3 ns |  7.53 ns |  4.98 ns | 2,261.5 ns |  1.04 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | Job-YFEFPZ | 10             | Default     | 3           |   370.4 ns |  1.27 ns |  0.67 ns |   370.3 ns |  0.17 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | Job-YFEFPZ | 10             | Default     | 3           |   632.0 ns |  5.33 ns |  3.52 ns |   632.1 ns |  0.29 | 0.0782 |    1320 B |        1.34 |
|                              |            |                |             |             |            |          |          |            |       |        |           |             |
| SimpleRoute_SingleCondition  | MediumRun  | 15             | 2           | 10          | 2,187.6 ns | 12.08 ns | 16.53 ns | 2,179.7 ns |  1.00 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | MediumRun  | 15             | 2           | 10          | 1,643.5 ns |  4.92 ns |  7.06 ns | 1,642.9 ns |  0.75 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | MediumRun  | 15             | 2           | 10          | 1,631.2 ns |  7.45 ns | 10.19 ns | 1,631.1 ns |  0.75 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | MediumRun  | 15             | 2           | 10          | 2,251.2 ns |  8.68 ns | 12.17 ns | 2,243.4 ns |  1.03 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | MediumRun  | 15             | 2           | 10          | 2,249.1 ns |  6.08 ns |  8.52 ns | 2,249.4 ns |  1.03 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | MediumRun  | 15             | 2           | 10          |   366.2 ns |  4.19 ns |  5.73 ns |   362.5 ns |  0.17 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | MediumRun  | 15             | 2           | 10          |   623.5 ns |  5.48 ns |  8.04 ns |   624.8 ns |  0.29 | 0.0782 |    1320 B |        1.34 |
