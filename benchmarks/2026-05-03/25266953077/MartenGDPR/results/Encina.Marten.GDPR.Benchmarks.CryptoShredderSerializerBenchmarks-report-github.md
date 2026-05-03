```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]    : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                    | Mean       | Error    | StdDev    | Median     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|---------:|----------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii    |   606.3 ns |  1.13 ns |   1.58 ns |   606.0 ns |  1.00 |    0.00 |    1 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_NonPii   |   659.1 ns |  4.38 ns |   6.56 ns |   658.1 ns |  1.09 |    0.01 |    2 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_PiiEvent | 8,419.5 ns | 77.56 ns | 108.72 ns | 8,342.8 ns | 13.89 |    0.18 |    3 | 0.1678 |    2869 B |       10.55 |
