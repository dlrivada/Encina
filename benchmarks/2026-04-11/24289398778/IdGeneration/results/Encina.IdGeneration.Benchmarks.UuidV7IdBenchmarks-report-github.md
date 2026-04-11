```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method             | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev   | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |------------ |-----------:|---------:|---------:|-----------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 3           | 1,094.5 ns |  2.25 ns |  1.49 ns | 1,094.8 ns |  1.01 |    0.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 3           |   598.9 ns |  1.79 ns |  1.19 ns |   598.2 ns |  0.55 |    0.00 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 3           | 1,084.6 ns |  3.08 ns |  1.83 ns | 1,083.8 ns |  1.00 |    0.00 |         - |          NA |
|                    |            |                |             |             |            |          |          |            |       |         |           |             |
| Generate_GetValue  | MediumRun  | 15             | 2           | 10          | 1,080.3 ns |  2.85 ns |  4.00 ns | 1,079.9 ns |  0.97 |    0.03 |         - |          NA |
| NewGuid_Comparison | MediumRun  | 15             | 2           | 10          |   598.6 ns |  0.28 ns |  0.39 ns |   598.5 ns |  0.54 |    0.02 |         - |          NA |
| Generate           | MediumRun  | 15             | 2           | 10          | 1,118.1 ns | 23.72 ns | 32.47 ns | 1,091.7 ns |  1.00 |    0.04 |         - |          NA |
