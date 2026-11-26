```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.75GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.858 ns |  1.604 ns | 0.0879 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 57.047 ns | 30.666 ns | 1.6809 ns |  7.26 |    0.20 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 44.568 ns | 11.961 ns | 0.6556 ns |  5.67 |    0.09 | 0.0091 |     152 B |        6.33 |
