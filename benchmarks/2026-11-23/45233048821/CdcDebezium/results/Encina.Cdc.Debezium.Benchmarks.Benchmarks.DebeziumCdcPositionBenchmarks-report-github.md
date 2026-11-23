```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.17GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  4.304 ns | 0.3896 ns | 0.0214 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 27.324 ns | 6.9555 ns | 0.3813 ns |  6.35 |    0.08 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 20.938 ns | 5.7194 ns | 0.3135 ns |  4.86 |    0.07 | 0.0091 |     152 B |        6.33 |
