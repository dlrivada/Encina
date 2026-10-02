```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                  | Mean      | Error       | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------ |----------:|------------:|----------:|------:|--------:|----------:|------------:|
| MarkAsProcessedAsync    | 223.09 μs |   783.63 μs | 42.954 μs |  1.23 |    0.21 |  11.97 KB |        1.20 |
| GetExpiredMessagesAsync | 808.65 μs | 1,090.30 μs | 59.763 μs |  4.45 |    0.35 | 287.61 KB |       28.72 |
| AddAsync                |  50.39 μs |   146.83 μs |  8.048 μs |  0.28 |    0.04 |   2.85 KB |        0.28 |
| GetMessageAsync         | 181.90 μs |   176.64 μs |  9.682 μs |  1.00 |    0.06 |  10.02 KB |        1.00 |
