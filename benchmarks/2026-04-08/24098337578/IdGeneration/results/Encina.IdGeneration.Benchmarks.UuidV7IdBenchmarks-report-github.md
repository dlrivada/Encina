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
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 3           | 1,091.8 ns |  1.76 ns |  1.05 ns | 1,091.8 ns |  1.00 |    0.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 3           |   604.8 ns |  1.50 ns |  0.90 ns |   604.8 ns |  0.55 |    0.00 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 3           | 1,092.8 ns |  3.53 ns |  2.34 ns | 1,092.7 ns |  1.00 |    0.00 |         - |          NA |
|                    |            |                |             |             |            |          |          |            |       |         |           |             |
| Generate_GetValue  | MediumRun  | 15             | 2           | 10          | 1,096.6 ns | 11.21 ns | 16.77 ns | 1,091.2 ns |  0.97 |    0.03 |         - |          NA |
| NewGuid_Comparison | MediumRun  | 15             | 2           | 10          |   606.6 ns |  1.76 ns |  2.47 ns |   605.4 ns |  0.54 |    0.02 |         - |          NA |
| Generate           | MediumRun  | 15             | 2           | 10          | 1,131.1 ns | 22.33 ns | 32.72 ns | 1,154.9 ns |  1.00 |    0.04 |         - |          NA |
