```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev   | Median     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |-----------:|---------:|---------:|-----------:|------:|--------:|-------:|----------:|------------:|
| SimpleRoute_SingleCondition  | Job-YFEFPZ | 10             | Default     | 3           | 1,989.9 ns |  9.37 ns |  5.58 ns | 1,993.2 ns |  1.00 |    0.00 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | Job-YFEFPZ | 10             | Default     | 3           | 1,407.1 ns |  3.64 ns |  2.16 ns | 1,407.1 ns |  0.71 |    0.00 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | Job-YFEFPZ | 10             | Default     | 3           | 1,410.7 ns |  5.90 ns |  3.91 ns | 1,411.8 ns |  0.71 |    0.00 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | Job-YFEFPZ | 10             | Default     | 3           | 2,090.2 ns | 10.65 ns |  5.57 ns | 2,091.9 ns |  1.05 |    0.00 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | Job-YFEFPZ | 10             | Default     | 3           | 2,087.8 ns |  9.91 ns |  6.55 ns | 2,087.3 ns |  1.05 |    0.00 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | Job-YFEFPZ | 10             | Default     | 3           |   380.1 ns |  5.54 ns |  3.66 ns |   381.0 ns |  0.19 |    0.00 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | Job-YFEFPZ | 10             | Default     | 3           |   669.8 ns |  7.64 ns |  5.05 ns |   671.1 ns |  0.34 |    0.00 | 0.0782 |    1320 B |        1.34 |
|                              |            |                |             |             |            |          |          |            |       |         |        |           |             |
| SimpleRoute_SingleCondition  | MediumRun  | 15             | 2           | 10          | 2,018.0 ns | 16.96 ns | 24.86 ns | 2,034.4 ns |  1.00 |    0.02 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | MediumRun  | 15             | 2           | 10          | 1,407.9 ns |  3.03 ns |  4.34 ns | 1,408.6 ns |  0.70 |    0.01 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | MediumRun  | 15             | 2           | 10          | 1,399.2 ns |  5.75 ns |  8.43 ns | 1,399.0 ns |  0.69 |    0.01 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | MediumRun  | 15             | 2           | 10          | 2,066.6 ns |  5.77 ns |  8.64 ns | 2,066.2 ns |  1.02 |    0.01 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | MediumRun  | 15             | 2           | 10          | 2,101.1 ns |  6.29 ns |  9.02 ns | 2,102.0 ns |  1.04 |    0.01 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | MediumRun  | 15             | 2           | 10          |   386.9 ns |  4.18 ns |  6.13 ns |   385.0 ns |  0.19 |    0.00 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | MediumRun  | 15             | 2           | 10          |   630.2 ns |  8.21 ns | 11.77 ns |   630.2 ns |  0.31 |    0.01 | 0.0782 |    1320 B |        1.34 |
