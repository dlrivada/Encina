```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,207.4 ns |  4.06 ns | 2.42 ns |  1.06 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   613.2 ns |  0.73 ns | 0.48 ns |  0.54 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,139.8 ns |  6.11 ns | 3.64 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,210.3 ns | 19.58 ns | 1.07 ns |  0.99 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   613.5 ns | 47.36 ns | 2.60 ns |  0.50 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,218.2 ns | 35.71 ns | 1.96 ns |  1.00 |         - |          NA |
