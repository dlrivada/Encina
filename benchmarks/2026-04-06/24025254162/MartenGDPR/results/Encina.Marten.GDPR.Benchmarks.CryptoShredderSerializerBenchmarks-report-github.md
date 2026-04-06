```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                    | Mean       | Error     | StdDev  | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|----------:|--------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii    |   630.8 ns |  10.42 ns | 0.57 ns |  1.00 |    0.00 |    1 | 0.0105 |     272 B |        1.00 |
| CryptoSerializer_NonPii   |   661.6 ns |  16.81 ns | 0.92 ns |  1.05 |    0.00 |    1 | 0.0105 |     272 B |        1.00 |
| CryptoSerializer_PiiEvent | 9,160.4 ns | 171.58 ns | 9.40 ns | 14.52 |    0.02 |    2 | 0.1068 |    2869 B |       10.55 |
