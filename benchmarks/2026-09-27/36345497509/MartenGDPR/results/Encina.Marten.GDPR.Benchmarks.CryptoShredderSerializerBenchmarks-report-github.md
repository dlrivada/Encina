```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                    | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii    |   597.4 ns | 104.10 ns |  5.71 ns |  1.00 |    0.01 |    1 | 0.0105 |     272 B |        1.00 |
| CryptoSerializer_NonPii   |   624.8 ns |   8.60 ns |  0.47 ns |  1.05 |    0.01 |    1 | 0.0105 |     272 B |        1.00 |
| CryptoSerializer_PiiEvent | 8,919.9 ns | 954.56 ns | 52.32 ns | 14.93 |    0.14 |    2 | 0.1068 |    2869 B |       10.55 |
