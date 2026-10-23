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
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,192.5 ns |  6.55 ns | 4.33 ns |  1.06 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   615.7 ns |  0.70 ns | 0.42 ns |  0.55 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,120.9 ns |  2.47 ns | 1.63 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,127.6 ns | 10.19 ns | 0.56 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   610.7 ns |  4.74 ns | 0.26 ns |  0.54 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,124.5 ns | 43.97 ns | 2.41 ns |  1.00 |         - |          NA |
