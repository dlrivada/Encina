```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  5.719 ns |  0.5886 ns | 0.0323 ns |  1.00 |    0.01 | 0.0003 |      24 B |        1.00 |
| FromBytes      | 43.100 ns | 44.1713 ns | 2.4212 ns |  7.54 |    0.37 | 0.0036 |     304 B |       12.67 |
| ToBytes        | 35.655 ns | 13.1565 ns | 0.7212 ns |  6.23 |    0.11 | 0.0018 |     152 B |        6.33 |
