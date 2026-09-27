```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.73GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                        | Mean      | Error    | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |----------:|---------:|----------:|------:|--------:|-------:|----------:|------------:|
| AddEncinaCdc_Registration     | 333.10 ns | 8.264 ns | 12.369 ns |  3.54 |    0.22 | 0.0114 |     976 B |        2.60 |
| BuildConfigurationFluentChain |  94.20 ns | 3.128 ns |  4.681 ns |  1.00 |    0.07 | 0.0044 |     376 B |        1.00 |
