```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.83GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|----------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,010.6 ns |   1.45 ns | 0.76 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   460.8 ns |   3.32 ns | 2.20 ns |  0.46 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,009.2 ns |   4.70 ns | 2.80 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |           |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,017.5 ns | 143.31 ns | 7.86 ns |  1.01 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   464.8 ns |  13.32 ns | 0.73 ns |  0.46 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,010.2 ns |  73.46 ns | 4.03 ns |  1.00 |         - |          NA |
