```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                    | Mean       | Error    | StdDev   | Median     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|---------:|---------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii    |   638.8 ns |  7.03 ns |  9.86 ns |   644.7 ns |  1.00 |    0.02 |    1 | 0.0105 |     272 B |        1.00 |
| CryptoSerializer_NonPii   |   672.3 ns |  4.43 ns |  6.49 ns |   674.0 ns |  1.05 |    0.02 |    2 | 0.0105 |     272 B |        1.00 |
| CryptoSerializer_PiiEvent | 9,234.9 ns | 59.34 ns | 85.11 ns | 9,236.5 ns | 14.46 |    0.26 |    3 | 0.1068 |    2869 B |       10.55 |
