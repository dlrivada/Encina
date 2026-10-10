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
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 985.9 ns |  4.94 ns | 2.94 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 444.3 ns |  7.33 ns | 4.85 ns |  0.45 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 982.2 ns |  6.65 ns | 4.40 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 987.2 ns | 16.58 ns | 0.91 ns |  1.01 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 449.8 ns | 21.33 ns | 1.17 ns |  0.46 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 979.3 ns | 56.45 ns | 3.09 ns |  1.00 |         - |          NA |
