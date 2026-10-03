```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,009.4 ns |  2.98 ns | 1.77 ns |  0.98 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   462.1 ns |  4.07 ns | 2.69 ns |  0.45 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,029.0 ns |  8.23 ns | 5.45 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,003.3 ns | 16.85 ns | 0.92 ns |  0.99 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   465.6 ns |  9.42 ns | 0.52 ns |  0.46 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,008.9 ns | 16.25 ns | 0.89 ns |  1.00 |         - |          NA |
