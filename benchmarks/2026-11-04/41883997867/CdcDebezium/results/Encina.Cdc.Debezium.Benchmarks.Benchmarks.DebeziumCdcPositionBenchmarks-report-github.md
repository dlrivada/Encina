```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.56GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.277 ns |  1.071 ns | 0.0587 ns |  1.00 |    0.01 | 0.0010 |      24 B |        1.00 |
| FromBytes      | 63.779 ns | 44.722 ns | 2.4514 ns |  8.77 |    0.30 | 0.0120 |     304 B |       12.67 |
| ToBytes        | 44.418 ns | 19.859 ns | 1.0885 ns |  6.10 |    0.14 | 0.0060 |     152 B |        6.33 |
