```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.08GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.523 ns |  3.721 ns | 0.2040 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 50.989 ns | 10.862 ns | 0.5954 ns |  6.78 |    0.17 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 42.676 ns |  7.051 ns | 0.3865 ns |  5.68 |    0.14 | 0.0091 |     152 B |        6.33 |
