```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.32GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                    | Mean       | Error    | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|---------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii    |   640.2 ns |  2.61 ns |   3.91 ns |  1.00 |    0.01 |    1 | 0.0105 |     272 B |        1.00 |
| CryptoSerializer_NonPii   |   672.8 ns |  0.96 ns |   1.41 ns |  1.05 |    0.01 |    2 | 0.0105 |     272 B |        1.00 |
| CryptoSerializer_PiiEvent | 9,114.8 ns | 82.26 ns | 123.12 ns | 14.24 |    0.21 |    3 | 0.1068 |    2869 B |       10.55 |
