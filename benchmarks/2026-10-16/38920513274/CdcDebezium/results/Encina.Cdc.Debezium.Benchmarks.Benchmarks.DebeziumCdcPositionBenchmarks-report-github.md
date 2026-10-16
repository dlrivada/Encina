```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.85GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.540 ns | 0.5596 ns | 0.0307 ns |  1.00 |    0.00 | 0.0010 |      24 B |        1.00 |
| FromBytes      | 66.910 ns | 5.1080 ns | 0.2800 ns |  8.87 |    0.04 | 0.0120 |     304 B |       12.67 |
| ToBytes        | 48.540 ns | 1.9679 ns | 0.1079 ns |  6.44 |    0.03 | 0.0060 |     152 B |        6.33 |
