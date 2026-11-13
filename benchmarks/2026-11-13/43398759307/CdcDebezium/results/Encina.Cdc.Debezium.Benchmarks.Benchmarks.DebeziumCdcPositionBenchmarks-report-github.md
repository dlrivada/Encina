```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.25GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  4.648 ns |  2.710 ns | 0.1485 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 26.612 ns | 15.803 ns | 0.8662 ns |  5.73 |    0.22 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 21.147 ns |  9.292 ns | 0.5093 ns |  4.55 |    0.16 | 0.0091 |     152 B |        6.33 |
