```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 977.0 ns |  4.09 ns | 2.44 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 442.7 ns |  6.55 ns | 4.33 ns |  0.45 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 980.2 ns |  6.67 ns | 3.97 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 989.9 ns | 96.77 ns | 5.30 ns |  1.01 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 445.8 ns | 11.27 ns | 0.62 ns |  0.46 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 977.9 ns | 25.32 ns | 1.39 ns |  1.00 |         - |          NA |
