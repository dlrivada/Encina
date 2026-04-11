```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method             | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev   | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |------------ |-----------:|---------:|---------:|-----------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 3           | 1,168.6 ns |  5.21 ns |  3.10 ns | 1,168.8 ns |  1.00 |    0.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 3           |   729.0 ns |  2.84 ns |  1.69 ns |   728.4 ns |  0.63 |    0.00 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 3           | 1,165.5 ns |  3.68 ns |  2.19 ns | 1,165.2 ns |  1.00 |    0.00 |         - |          NA |
|                    |            |                |             |             |            |          |          |            |       |         |           |             |
| Generate_GetValue  | MediumRun  | 15             | 2           | 10          | 1,206.4 ns | 31.77 ns | 44.53 ns | 1,246.5 ns |  1.01 |    0.05 |         - |          NA |
| NewGuid_Comparison | MediumRun  | 15             | 2           | 10          |   724.3 ns |  1.66 ns |  2.39 ns |   723.7 ns |  0.61 |    0.02 |         - |          NA |
| Generate           | MediumRun  | 15             | 2           | 10          | 1,193.7 ns | 26.82 ns | 38.47 ns | 1,163.7 ns |  1.00 |    0.04 |         - |          NA |
