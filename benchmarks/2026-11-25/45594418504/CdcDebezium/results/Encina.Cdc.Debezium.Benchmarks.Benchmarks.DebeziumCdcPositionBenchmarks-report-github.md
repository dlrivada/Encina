```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.11GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.266 ns |  2.159 ns | 0.1183 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 50.766 ns | 12.971 ns | 0.7110 ns |  6.99 |    0.13 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 40.931 ns |  2.271 ns | 0.1245 ns |  5.63 |    0.08 | 0.0091 |     152 B |        6.33 |
