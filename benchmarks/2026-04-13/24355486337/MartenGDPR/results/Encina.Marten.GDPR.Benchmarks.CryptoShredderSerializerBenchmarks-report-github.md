```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.82GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                    | Mean       | Error    | StdDev   | Median     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|---------:|---------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii    |   628.6 ns |  5.53 ns |  7.75 ns |   626.4 ns |  1.00 |    0.02 |    1 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_NonPii   |   688.7 ns | 21.51 ns | 30.15 ns |   668.7 ns |  1.10 |    0.05 |    2 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_PiiEvent | 9,133.5 ns | 24.94 ns | 34.96 ns | 9,128.5 ns | 14.53 |    0.18 |    3 | 0.1678 |    2869 B |       10.55 |
