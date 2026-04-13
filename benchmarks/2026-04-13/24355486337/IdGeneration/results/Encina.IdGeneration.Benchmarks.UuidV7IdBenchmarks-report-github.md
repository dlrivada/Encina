```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method             | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |------------ |-----------:|---------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 3           | 1,172.5 ns |  5.52 ns |  2.89 ns |  1.07 |    0.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 3           |   598.3 ns |  0.33 ns |  0.22 ns |  0.54 |    0.00 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 3           | 1,100.1 ns |  3.23 ns |  2.13 ns |  1.00 |    0.00 |         - |          NA |
|                    |            |                |             |             |            |          |          |       |         |           |             |
| Generate_GetValue  | MediumRun  | 15             | 2           | 10          | 1,141.2 ns | 16.93 ns | 24.28 ns |  1.04 |    0.02 |         - |          NA |
| NewGuid_Comparison | MediumRun  | 15             | 2           | 10          |   599.2 ns |  0.60 ns |  0.86 ns |  0.55 |    0.00 |         - |          NA |
| Generate           | MediumRun  | 15             | 2           | 10          | 1,096.6 ns |  5.11 ns |  7.65 ns |  1.00 |    0.01 |         - |          NA |
