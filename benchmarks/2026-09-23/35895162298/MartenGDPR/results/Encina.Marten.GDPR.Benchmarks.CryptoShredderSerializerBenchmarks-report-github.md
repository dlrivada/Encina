```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                    | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|------------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii    |   624.2 ns |    76.92 ns |  4.22 ns |  1.00 |    0.01 |    1 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_NonPii   |   631.8 ns |    13.30 ns |  0.73 ns |  1.01 |    0.01 |    1 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_PiiEvent | 8,323.8 ns | 1,162.52 ns | 63.72 ns | 13.34 |    0.12 |    2 | 0.1678 |    2869 B |       10.55 |
