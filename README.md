# SqliteDbContext Entity Generator
<p>
This project helps to encapsulate logic for randomly generating a valid entity in an Entity Framework DB Context.<br />
The user only has to define the relationship with PKs, FKs, and PKs that are contrainted as FKs as well. <br />
This project also references <a href="https://github.com/skenneth/AutoPopulate.git"> AutoPopulate</a> project which has yet to become a Nuget Package.
</p>
<br />
<h1>How it works</h1>
<p>
  <ol>
    <li>
      Autopopulate library crawls through properties of a generated instance and references a dictionary<ClassType:PropertyName, Func<PropertyType>> which calls the appropriate Function to assign the value. In this case, each function uses AutoFake library's randomization functionality for each property's type.
    </li>
    <li>
      Once the item is generated, all class refernces are set to null. Due to EFCore's DBContext's tracking, an exception can be thrown if the FK's id doesn't match the corresponding referenced instance's PK. This built-in logic helps to enforce relationship FK-constraints. So these are set to null, so that once items are added and saved, then the tracked FK reference will be supplied.
    </li>
    <li>
      All PKs and FKs are reset to -1 for short, int, long, null (string) so that these values will be tracked via a IKeySeeder helper when the user utilizes this interface to perform auto-increment on PKs or generate a random ID from FKs. The key seeder is not limited to a single PK and FK, but instead the set of PKs that are defined by the table schema.
    </li>
    <li>
      To make this robust and convenient, the user can create custom entities to add/updat. These action will be applied at this stage. Several conditions will be considered whe adding/updating with respect to previous operations such as auto-generation or custom entity.
    </li>
    <li>
      Once entity has been added/updated, the user's dependency resolution will be executed. This is primarly how the keys are generated.
    </li>
    <li>
      Finally, after all validation and dependencies are resolved, then the entity will be added/updated. A user can generate a random entity, or create N entities with ease, only worrying about establishing the foreign key constraints.
    </li>
  </ol>
</p>
