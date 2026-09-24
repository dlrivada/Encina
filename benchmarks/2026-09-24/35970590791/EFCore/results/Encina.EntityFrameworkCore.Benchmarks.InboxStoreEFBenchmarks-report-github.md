```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                  | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
| MarkAsProcessedAsync    | 152.16 μs | 285.035 μs | 15.624 μs |  1.31 |    0.15 |  11.97 KB |        1.20 |
| GetExpiredMessagesAsync | 901.71 μs | 884.956 μs | 48.507 μs |  7.78 |    0.69 | 287.61 KB |       28.72 |
| AddAsync                |  46.31 μs |   9.448 μs |  0.518 μs |  0.40 |    0.03 |   2.85 KB |        0.28 |
| GetMessageAsync         | 116.54 μs | 193.486 μs | 10.606 μs |  1.01 |    0.11 |  10.02 KB |        1.00 |
