```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method               | Job        | IterationCount | LaunchCount | Mean     | Error   | StdDev  | Ratio | Allocated | Alloc Ratio |
|--------------------- |----------- |--------------- |------------ |---------:|--------:|--------:|------:|----------:|------------:|
| Generate             | Job-YFEFPZ | 10             | Default     | 241.7 ns | 0.14 ns | 0.10 ns |  1.00 |         - |          NA |
| Generate_WithShardId | Job-YFEFPZ | 10             | Default     | 241.7 ns | 0.12 ns | 0.07 ns |  1.00 |         - |          NA |
| ExtractShardId       | Job-YFEFPZ | 10             | Default     | 239.6 ns | 0.09 ns | 0.06 ns |  0.99 |         - |          NA |
| GenerateAndGetValue  | Job-YFEFPZ | 10             | Default     | 241.6 ns | 0.09 ns | 0.06 ns |  1.00 |         - |          NA |
|                      |            |                |             |          |         |         |       |           |             |
| Generate             | ShortRun   | 3              | 1           | 241.6 ns | 2.16 ns | 0.12 ns |  1.00 |         - |          NA |
| Generate_WithShardId | ShortRun   | 3              | 1           | 241.6 ns | 2.02 ns | 0.11 ns |  1.00 |         - |          NA |
| ExtractShardId       | ShortRun   | 3              | 1           | 239.5 ns | 2.07 ns | 0.11 ns |  0.99 |         - |          NA |
| GenerateAndGetValue  | ShortRun   | 3              | 1           | 241.6 ns | 1.47 ns | 0.08 ns |  1.00 |         - |          NA |
