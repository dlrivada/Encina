```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                    | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| InnerSerializer_NonPii    |   625.9 ns | 132.42 ns |  7.26 ns |  1.00 |    0.01 |    1 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_NonPii   |   646.8 ns |  13.87 ns |  0.76 ns |  1.03 |    0.01 |    1 | 0.0162 |     272 B |        1.00 |
| CryptoSerializer_PiiEvent | 8,549.0 ns | 305.67 ns | 16.75 ns | 13.66 |    0.14 |    2 | 0.1678 |    2869 B |       10.55 |
